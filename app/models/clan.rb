class Clan < ApplicationRecord
	has_many :players
	has_many :marks, through: :players
	
	def to_param
		tag
	end
end
