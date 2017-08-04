class Tier < ApplicationRecord
	has_many :tanks
	has_many :marks, through: :tanks
end
