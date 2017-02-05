class VehicleType < ApplicationRecord
	has_many :tanks
	has_many :marks, through: :tanks
end
